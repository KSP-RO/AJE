if [ -z "${TARGET_PATH}" ] ; then
  echo 'Expected $TARGET_PATH to be defined but it is not' >&2
  exit 1
fi

if ! [ -f "${TARGET_PATH}" ] ; then
  echo 'Expected $TARGET_PATH to be a file but it is not' >&2
  exit 1
fi

if [ -z "${TARGET_DIR}" ] ; then
  echo 'Expected $TARGET_DIR to be defined but it is not' >&2
  exit 1
fi

if ! [ -d "${TARGET_DIR}" ] ; then
  echo 'Expected $TARGET_DIR to be a directory but it is not' >&2
  exit 1
fi

if [ -z "${TARGET_NAME}" ] ; then
  echo 'Expected $TARGET_NAME to be defined but it is not' >&2
  exit 1
fi

if [ -z "${PDB2MDB}" ] ; then
  echo '$PDB2MDB not found'
else
  if ! [ -f "${TARGET_DIR}/${TARGET_NAME}.pdb" ]; then
    echo "${TARGET_NAME}.pdb not found"
  else
    echo "Running '${PDB2MDB}'"
    "${PDB2MDB}" "${TARGET_PATH}"
  fi
fi
exit 0
